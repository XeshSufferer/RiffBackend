package main

import (
	"log"
	"time"

	rabbitmq "github.com/XeshSufferer/Rabbilite/rabbilite"
)

var (
	rabbitUri = "amqp://guest:guest@rabbitmq:5672/"
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

	go func() {

	}()

	for {
		time.Sleep(1 * time.Minute)
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
