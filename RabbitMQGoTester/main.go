package main

import (
	"log"
	"time"

	rabbitmq "github.com/XeshSufferer/Rabbilite/rabbilite"
)

var (
	rabbitUri string = "amqp://guest:guest@rabbitmq:5672/"
)

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
			//log.Println("Recieved: " + string(message))
			producer.SendMessage("output", string("Hello!"))
			return nil
		})
		if err != nil {
			log.Fatal("Failed to start consumer:", err)
		}
	}()

	select {}
}
