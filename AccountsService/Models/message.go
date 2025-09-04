package models

import (
	"time"

	"go.mongodb.org/mongo-driver/bson/primitive"
)

type Message struct {
	ID         primitive.ObjectID `bson:"_id,omitempty" json:"id,omitempty"`
	ChatId     primitive.ObjectID `bson:"chat_id" json:"chat_id"`
	SenderId   primitive.ObjectID `bson:"sender_id" json:"sender_id"`
	Text       string             `bson:"text" json:"text"`
	Created    time.Time          `bson:"created" json:"created"`
	IsModified bool               `bson:"is_modified" json:"is_modified"`
}
