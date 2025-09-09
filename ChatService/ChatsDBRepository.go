package main

import (
	models "ChatsService/Models"
	"context"
	"log"
	"time"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

type ChatsDBRepository struct {
	Uri    string
	client mongo.Client
}

func CreateDBRepository(uri string) ChatsDBRepository {
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	client, err := mongo.Connect(ctx, options.Client().ApplyURI(uri))
	if err != nil {
		log.Fatal(err)
	}

	db := ChatsDBRepository{Uri: uri, client: *client}
	return db
}

func (repo ChatsDBRepository) GetChatByHexId(id string) models.Chat {
	filter := bson.M{"_id": id}

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	var chat models.Chat

	repo.client.Database("main").Collection("chats").FindOne(ctx, filter).Decode(&chat)
	return chat
}

func (repo ChatsDBRepository) CreateChat(chat models.Chat) error {

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	_, err := repo.client.Database("main").Collection("chats").InsertOne(ctx, chat)

	return err
}

func (repo ChatsDBRepository) UpdateChat(chat *models.Chat) (models.Chat, error) {

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	result, err := repo.client.Database("main").Collection("chats").UpdateByID(ctx, chat.ID, chat)
	if err != nil {
		log.Printf("Update user failed, %v", result)
	}
	return *chat, nil

}

func (repo ChatsDBRepository) DeleteChat(chat models.Chat) bool {
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()
	_, err := repo.client.Database("main").Collection("chats").DeleteOne(ctx, (bson.M{"_id": chat.ID}))

	if err != nil {
		log.Printf("Delete account error: %v", err)
		return false
	}
	return true
}
