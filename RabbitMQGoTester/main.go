package main

import (
	"encoding/json"
	"log"
	"time"

	rabbitmq "github.com/XeshSufferer/Rabbilite/rabbilite"
)

var (
	rabbitUri string = "amqp://guest:guest@rabbitmq:5672/"
)

type TestMessage struct {
	Message       string `json:"Message"`       // Поле будет сериализовано как "Message"
	CorrelationId string `json:"CorrelationId"` // Поле будет сериализовано как "CorrelationId"
}

func main() {

	time.Sleep(1500 * time.Millisecond)
	producer, errProducer := rabbitmq.NewProducer(rabbitUri)
	if errProducer != nil {
		log.Fatal(errProducer)
	}
	defer producer.Close()

	consumer, errConsumer := rabbitmq.NewConsumer(rabbitUri)
	if errConsumer != nil {
		log.Fatal(errConsumer)
	}
	defer consumer.Close()

	go func() {
		err := consumer.StartConsuming("input", func(message []byte) error {
			if message == nil {
				return nil
			}

			var inputMsg TestMessage

			err := json.Unmarshal(message, &inputMsg)
			if err != nil {
				panic("json deserialize error")
			}
			//log.Println("Recieved: " + string(message))
			producer.SendMessage("output", TestMessage{Message: "Hello from go!", CorrelationId: inputMsg.CorrelationId})
			return nil
		})
		if err != nil {
			log.Fatal("Failed to start consumer:", err)
		}
	}()

	select {}
}
