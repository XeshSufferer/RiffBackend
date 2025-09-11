package main

import (
	models "MessageServvice/Models"
	"context"
	"errors"
	"log"
	"time"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

type MessagesDBRepository struct {
	Uri    string
	client mongo.Client
}

func CreateMessagesDBRepository(uri string) MessagesDBRepository {
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	client, err := mongo.Connect(ctx, options.Client().ApplyURI(uri))
	if err != nil {
		log.Fatal(err)
	}

	repo := MessagesDBRepository{Uri: uri, client: *client}
	createMessagesIndexes(repo)
	return repo
}

func createMessagesIndexes(repo MessagesDBRepository) error {
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	indexModels := []mongo.IndexModel{
		{
			Keys:    bson.D{{Key: "last_Message_Recieve_Time", Value: -1}},
			Options: options.Index().SetBackground(true),
		},
	}

	_, err := repo.client.Database("main").Collection("chats_messages").Indexes().CreateMany(ctx, indexModels)
	return err
}

func (repo MessagesDBRepository) GetByChatHexID(chatHexId string) (models.ChatMessageCollection, error) {
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	objectID, err := primitive.ObjectIDFromHex(chatHexId)
	if err != nil {
		return models.ChatMessageCollection{}, err
	}

	var collection models.ChatMessageCollection
	err = repo.client.Database("main").Collection("chats_messages").FindOne(ctx, bson.M{"_id": objectID}).Decode(&collection)
	if err == mongo.ErrNoDocuments {
		return models.ChatMessageCollection{}, nil
	}
	return collection, err
}

func (repo MessagesDBRepository) ReplaceCollection(collection models.ChatMessageCollection) error {
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	_, err := repo.client.Database("main").Collection("chats_messages").ReplaceOne(
		ctx,
		bson.M{"_id": collection.ChatId},
		collection,
		options.Replace().SetUpsert(true),
	)
	return err
}

func (repo MessagesDBRepository) AppendMessage(chatHexId string, message models.Message) error {
	if chatHexId == "" {
		return errors.New("chat id is empty")
	}

	chatID, err := primitive.ObjectIDFromHex(chatHexId)
	if err != nil {
		return err
	}

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	update := bson.M{
		"$push": bson.M{"messages": message},
		"$set":  bson.M{"last_Message_Recieve_Time": time.Now()},
	}

	_, err = repo.client.Database("main").Collection("chats_messages").UpdateByID(
		ctx,
		chatID,
		update,
		options.Update().SetUpsert(true),
	)
	return err
}

func (repo MessagesDBRepository) DeleteByChatHexID(chatHexId string) (bool, error) {
	if chatHexId == "" {
		return false, errors.New("chat id is empty")
	}

	chatID, err := primitive.ObjectIDFromHex(chatHexId)
	if err != nil {
		return false, err
	}

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	result, err := repo.client.Database("main").Collection("chats_messages").DeleteOne(ctx, bson.M{"_id": chatID})
	if err != nil {
		log.Printf("Delete chat messages error: %v", err)
		return false, err
	}
	return result.DeletedCount > 0, nil
}
