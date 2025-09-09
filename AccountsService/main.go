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
	dbUri     string = "mongodb://database:27017/"
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

	c, p, errConnect := connectWithRetry()

	defer c.Close()
	defer p.Close()

	db := CreateDBRepository(dbUri)

	go func() {
		registerCoreErr := c.StartConsuming("Riff.Core.Accounts.Register.Input", func(message []byte) error {

			var registerdata models.UserRegisterData
			jsonErr := json.Unmarshal(message, &registerdata)
			if jsonErr != nil {
				log.Fatalf("json deserialize in register error ")
				return jsonErr
			}

			usr, err := db.CreateAccount(registerdata.Nickname, registerdata.Nickname, registerdata.Password)
			if err != nil {
				log.Printf("Failed to create account: %v", err)
			}

			if usr == nil {
				return errors.New("Usr is null")
			} else {
				usr.CorrelationId = registerdata.CorrelationID
			}

			for i := 0; i < 3; i++ {
				err = p.SendMessage("Riff.Core.Accounts.Register.Output", usr)
				if err != nil {
					log.Printf("Failed to send message (attempt %d): %v", i+1, err)
					time.Sleep(1 * time.Second)
					continue
				}
				break
			}

			//p.SendMessage("Riff.Core.Accounts.Register.Output", usr)

			return nil
		})

		loginCoreErr := c.StartConsuming("Riff.Core.Accounts.Login.Input", func(message []byte) error {

			var logindata models.UserLoginData
			jsonErr := json.Unmarshal(message, &logindata)
			if jsonErr != nil {
				log.Printf("json deserialize in Login error %v", jsonErr)
			}

			correlationID := strings.Trim(logindata.CorellationId, "\"")

			usr, err := db.Login(logindata.Login, logindata.Password)
			if err != nil {
				log.Printf("Failed to login account: %v", err)
			}

			if usr == nil {
				usr = &models.User{CorrelationId: logindata.CorellationId, PasswordHash: "NULL"}
			} else {
				usr.CorrelationId = correlationID
			}

			for i := 0; i < 3; i++ {
				err = p.SendMessage("Riff.Core.Accounts.Login.Output", usr)
				if err != nil {
					log.Printf("Failed to send message (attempt %d): %v", i+1, err)
					time.Sleep(1 * time.Second)
					continue
				}
				break
			}

			//p.SendMessage("Riff.Core.Accounts.Login.Output", usr)
			return nil
		})

		getbyidCoreErrConsumer := c.StartConsuming("Riff.Core.Accounts.GetByID.Input", func(message []byte) error {

			var IdDto models.UserIDDTO
			jsonErr := json.Unmarshal(message, &IdDto)
			if jsonErr != nil {
				log.Fatalf("json deserialize in getbyid error ")
				return jsonErr
			}

			usr := db.GetUserByHexID(IdDto.ID)

			usr.CorrelationId = IdDto.CorrelationID

			for i := 0; i < 3; i++ {
				getbyidCoreErr := p.SendMessage("Riff.Core.Accounts.GetByID.Output", usr)
				if getbyidCoreErr != nil {
					log.Printf("Failed to send message (attempt %d): %v", i+1, getbyidCoreErr)
					time.Sleep(1 * time.Second)
					continue
				}
				break
			}

			//p.SendMessage("Riff.Core.Accounts.GetByID.Output", usr)

			return nil
		})

		getbyidErrConsumer := c.StartConsuming("Riff.Accounts.GetByID.Input", func(message []byte) error {

			var IdDto models.UserIDDTO
			jsonErr := json.Unmarshal(message, &IdDto)
			if jsonErr != nil {
				log.Fatalf("json deserialize in getbyid error ")
				return jsonErr
			}

			usr := db.GetUserByHexID(IdDto.ID)

			usr.CorrelationId = IdDto.CorrelationID

			for i := 0; i < 3; i++ {
				getbyidErr := p.SendMessage("Riff.Accounts.GetByID.Output", usr)
				if getbyidErr != nil {
					log.Printf("Failed to send message (attempt %d): %v", i+1, getbyidErr)
					time.Sleep(1 * time.Second)
					continue
				}
				break
			}

			//p.SendMessage("Riff.Accounts.GetByID.Output", usr)

			return nil
		})

		// errors

		if getbyidErrConsumer != nil {
			log.Printf("Failed to start consuming: %v", getbyidErrConsumer)
		}

		if getbyidCoreErrConsumer != nil {
			log.Printf("Failed to start consuming: %v", getbyidCoreErrConsumer)
		}

		if registerCoreErr != nil {
			log.Printf("Failed to start consuming: %v", registerCoreErr)
		}

		if loginCoreErr != nil {
			log.Printf("Failed to start consuming: %v", loginCoreErr)
		}

	}()

	for {
		time.Sleep(1 * time.Minute)
		// Проверяем соединение периодически
		if !c.IsConnected() || !p.IsConnected() {
			log.Println("Connection lost, attempting to reconnect...")
			c.Close()
			p.Close()

			c, p, errConnect = connectWithRetry()
			if errConnect != nil {
				log.Printf("Reconnection failed: %v", errConnect)
			} else {
				log.Println("Reconnected successfully")
			}
		}
	}
}
