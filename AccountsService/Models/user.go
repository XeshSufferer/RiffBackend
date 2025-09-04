package models

import (
	"time"

	"go.mongodb.org/mongo-driver/bson/primitive"
)

type User struct {
	ID            primitive.ObjectID   `bson:"_id,omitempty" json:"id,omitempty"`
	ChatsIds      []primitive.ObjectID `bson:"chats_ids" json:"chats_ids"`
	Name          string               `bson:"name" json:"name"`
	Login         string               `bson:"login" json:"login"`
	PasswordHash  string               `bson:"password_hash" json:"-"`
	Created       time.Time            `bson:"created" json:"created"`
	CorrelationId string               `bson:"-" json:"correlation_id"`
}
