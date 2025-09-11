package models

import (
	"time"

	"go.mongodb.org/mongo-driver/bson/primitive"
)

type ChatMessageCollection struct {
	ChatId      primitive.ObjectID `bson:"_id,omitempty" json:"id,omitempty"`
	Messages    []Message          `bson:"messages" json:"messages"`
	LastMessage time.Time          `bson:"last_Message_Recieve_Time" json:"last_Message_Recieve_Time"`
}
