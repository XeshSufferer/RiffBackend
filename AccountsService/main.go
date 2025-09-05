package main

import (
	models "AccountsService/Models"
	"encoding/json"
	"errors"
	"log"
	"runtime"
	"strings"
	"time"

	rabbitmq "github.com/XeshSufferer/Rabbilite/rabbilite"
)

var (
	rabbitUri string = "amqp://guest:guest@rabbitmq:5672/"
)

func main() {

	runtime.GOMAXPROCS(0)

	time.Sleep(3000 * time.Millisecond)

	connectWithRetry := func() (*rabbitmq.Consumer, *rabbitmq.Producer, error) {
		var c *rabbitmq.Consumer
		var p *rabbitmq.Producer
		var err error

		for i := 0; i < 5; i++ {
			c, err = rabbitmq.NewConsumer(rabbitUri)
			if err != nil {
				log.Printf("Failed to create consumer (attempt %d): %v", i+1, err)
				time.Sleep(2 * time.Second)
				continue
			}

			p, err = rabbitmq.NewProducer(rabbitUri)
			if err != nil {
				log.Printf("Failed to create producer (attempt %d): %v", i+1, err)
				c.Close()
				time.Sleep(2 * time.Second)
				continue
			}

			return c, p, nil
		}
		return nil, nil, err
	}

	c, p, err := connectWithRetry()

	defer c.Close()
	defer p.Close()

	db := CreateDBRepository("mongodb://database:27017/")

	go func() {
		err := c.StartConsuming("Riff.Core.Accounts.Input.Register", func(message []byte) error {
			correlationID := strings.Trim(string(message), "\"")

			usr, err := db.CreateAccount("namename", "loginlogin", "passpass")
			if err != nil {
				log.Printf("Failed to create account: %v", err)
				return err
			}

			usr.CorrelationId = correlationID

			if usr == nil {
				return errors.New("Usr is null")
			}

			for i := 0; i < 3; i++ {
				err = p.SendMessage("Riff.Core.Accounts.Output.Register", usr)
				if err != nil {
					log.Printf("Failed to send message (attempt %d): %v", i+1, err)
					time.Sleep(1 * time.Second)
					continue
				}
				break
			}

			p.SendMessage("Riff.Core.Accounts.Output.Register", usr)

			return nil
		})

		nocoreErr := c.StartConsuming("Riff.Core.Accounts.Input.Login", func(message []byte) error {

			var logindata models.UserLoginData
			jsonErr := json.Unmarshal(message, &logindata)
			if jsonErr != nil {
				log.Fatalf("json deserialize in Login error ")
			}

			correlationID := strings.Trim(logindata.CorellationId, "\"")

			usr, err := db.Login(logindata.Login, logindata.Password)
			if err != nil {
				log.Printf("Failed to login account: %v", err)
				return err
			}

			usr.CorrelationId = correlationID

			if usr == nil {
				return errors.New("Usr is null")
			}

			for i := 0; i < 3; i++ {
				err = p.SendMessage("Riff.Accounts.Login.Output", usr)
				if err != nil {
					log.Printf("Failed to send message (attempt %d): %v", i+1, err)
					time.Sleep(1 * time.Second)
					continue
				}
				break
			}

			p.SendMessage("Riff.Core.Accounts.Login.Output", usr)

			return nil
		})

		// errors
		if err != nil {
			log.Printf("Failed to start consuming: %v", err)
		}

		if nocoreErr != nil {
			log.Printf("Failed to start consuming: %v", nocoreErr)
		}
	}()

	for {
		time.Sleep(1 * time.Minute)
		// Проверяем соединение периодически
		if !c.IsConnected() || !p.IsConnected() {
			log.Println("Connection lost, attempting to reconnect...")
			c.Close()
			p.Close()

			c, p, err = connectWithRetry()
			if err != nil {
				log.Printf("Reconnection failed: %v", err)
			} else {
				log.Println("Reconnected successfully")
			}
		}
	}

}
