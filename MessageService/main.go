package main

import (
	models "MessageServvice/Models"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"time"

	rabbitmq "github.com/XeshSufferer/Rabbilite/rabbilite"
)

var (
	rabbitUri        = "amqp://guest:guest@rabbitmq:5672/"
	dbUri     string = "mongodb://database:27017/"
)

func main() {

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

	db := CreateMessagesDBRepository(dbUri)

	consuming := func() {

		c.StartConsuming("Riff.Core.Messages.SendMessage.Input", func(message []byte) error {

			var msg models.Message
			err := json.Unmarshal(message, &msg)
			if err != nil {
				log.Println(err)
				return nil
			}

			if !ChatExistInUser(msg.SenderId.Hex(), msg.ChatId.Hex()) {
				p.SendMessage("Riff.Core.Messages.SendMessage.Output", models.Message{CorrelationId: msg.CorrelationId})
				return nil
			}

			db.AppendMessage(msg.ChatId.Hex(), msg)

			for i := 0; i < 3; i++ {
				err = p.SendMessage("Riff.Core.Messages.SendMessage.Output", msg)
				if err != nil {
					log.Printf("Failed to send message (attempt %d): %v", i+1, err)
					time.Sleep(1 * time.Second)
					continue
				}
				break
			}
			return nil
		})

	}

	go consuming()

	for {
		time.Sleep(1 * time.Minute)
		if !c.IsConnected() || !p.IsConnected() {
			log.Println("Connection lost, attempting to reconnect...")
			c.Close()
			p.Close()

			c, p, errConnect = connectWithRetry()
			go consuming()

			if errConnect != nil {
				log.Printf("Reconnection failed: %v", errConnect)
			} else {
				log.Println("Reconnected successfully")
			}
		}
	}

}

func ChatExistInUser(userid string, chatid string) bool {
	url := fmt.Sprintf("http://accounts:8081/userHaveAChatById/%s/%s", userid, chatid)

	resp, err := http.Get(url)
	if err != nil {
		return false
	}
	defer resp.Body.Close()

	if resp.StatusCode == 404 {
		return false
	}

	if resp.StatusCode != 200 {
		return false
	}

	var response models.BooleanResponse
	if err := json.NewDecoder(resp.Body).Decode(&response); err != nil {
		return false
	}

	return response.Success
}
