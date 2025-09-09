package main

import (
	models "ChatsService/Models"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"runtime"
	"time"

	rabbitmq "github.com/XeshSufferer/Rabbilite/rabbilite"
	"go.mongodb.org/mongo-driver/bson/primitive"
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
		c.StartConsuming("Riff.Chats.Creating.Input", func(message []byte) error {

			var requestDto models.ChatCreatingRequestDTO
			jsonErr := json.Unmarshal(message, &requestDto)

			log.Println("json deserialize")
			if jsonErr != nil {
				log.Printf("json deserialize in creatingchat error %v", jsonErr)
				return jsonErr
			}

			_requestedId := getUserIdByName(requestDto.RequestedUsername)

			if _requestedId == "" {
				return nil
			}

			requesterObjId, err := primitive.ObjectIDFromHex(requestDto.RequesterId)
			if err != nil {
				log.Printf("Ошибка преобразования RequesterId: %v", err)
				return err
			}
			requestedObjId, err := primitive.ObjectIDFromHex(_requestedId)
			if err != nil {
				log.Printf("Ошибка преобразования RequestedId: %v", err)
				return err
			}
			err = db.CreateChat(requesterObjId, requestedObjId)
			if err != nil {
				log.Print(err)
				return err
			}

			return nil
		})
	}()

	// Reconnect
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

func checkUserExists(username string) bool {
	url := fmt.Sprintf("http://accounts:8081/userExistByName/%s", username)

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

func checkUserExistsById(id string) bool {
	url := fmt.Sprintf("http://accounts:8081/userExistById/%s", id)

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

func getUserIdByName(id string) string {
	url := fmt.Sprintf("http://accounts:8081/getUserIdByName/%s", id)

	resp, err := http.Get(url)
	if err != nil {
		return ""
	}
	defer resp.Body.Close()

	if resp.StatusCode == 404 {
		return ""
	}

	if resp.StatusCode != 200 {
		return ""
	}

	var response models.StringResponse
	if err := json.NewDecoder(resp.Body).Decode(&response); err != nil {
		return ""
	}

	return response.Result
}
