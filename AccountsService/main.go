package main

import (
	"log"
	"strings"
	"time"

	rabbitmq "github.com/XeshSufferer/Rabbilite/rabbilite"
)

var (
	rabbitUri string = "amqp://guest:guest@rabbitmq:5672/"
)

func main() {

	time.Sleep(1500 * time.Millisecond)

	c, err := rabbitmq.NewConsumer(rabbitUri)
	p, _ := rabbitmq.NewProducer(rabbitUri)
	db := CreateDBRepository("mongodb://database:27017/")

	log.Println("Initialized")

	if err != nil {
		panic("host is death")
	}

	c.StartConsuming("Riff.Core.Accounts.Input.Register", func(message []byte) error {
		log.Println("Event Recieved ")
		usr, _ := db.CreateAccount("namename", "loginlogin", "passpass")
		usr.CorrelationId = strings.ReplaceAll(string(message), "\"", "")
		log.Println(strings.ReplaceAll(string(message), "\"", ""))
		log.Println("Account created")
		p.SendMessage("Riff.Core.Accounts.Output.Register", usr)
		return nil
	})

	select {}

}
