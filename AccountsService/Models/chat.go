package models

import (
	"time"

	"go.mongodb.org/mongo-driver/bson/primitive"
)

type Chat struct {
	ID          primitive.ObjectID   `bson:"_id,omitempty" json:"id,omitempty"`
	Name        string               `bson:"name" json:"name"`
	Description string               `bson:"description" json:"description"`
	MembersId   []primitive.ObjectID `bson:"members_id" json:"members_id"`
	Created     time.Time            `bson:"created" json:"created"`
}
