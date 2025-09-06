package main

import (
	models "AccountsService/Models"
	"context"
	"errors"
	"log"
	"time"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
	"golang.org/x/crypto/bcrypt"
)

type AccountsDBRepository struct {
	Uri    string
	client mongo.Client
}

func CreateDBRepository(uri string) AccountsDBRepository {
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	client, err := mongo.Connect(ctx, options.Client().ApplyURI(uri))
	if err != nil {
		log.Fatal(err)
	}

	return AccountsDBRepository{Uri: uri, client: *client}

}

func (repo AccountsDBRepository) GetUserByName(name string) models.User {

	filter := bson.M{"name": name}

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	var user models.User

	repo.client.Database("main").Collection("users").FindOne(ctx, filter).Decode(&user)
	return user
}

func (repo AccountsDBRepository) GetUserByHexID(_id string) models.User {

	id, err := primitive.ObjectIDFromHex(_id)
	if err != nil {
		log.Fatalf("From string to object Id decode error %v", err)
	}

	filter := bson.M{"_id": id}

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	var user models.User

	repo.client.Database("main").Collection("users").FindOne(ctx, filter).Decode(&user)
	return user
}

func (repo AccountsDBRepository) CreateAccount(nick string, login string, password string) (*models.User, error) {

	hashedPassword, err := bcrypt.GenerateFromPassword([]byte(password+login), bcrypt.MinCost)
	if err != nil {
		log.Fatalf("password hashing failed: %v", err)
	}

	user := models.User{
		ID:           primitive.NewObjectID(),
		Login:        login,
		Name:         nick,
		PasswordHash: string(hashedPassword),
		Created:      time.Now(),
		ChatsIds:     []primitive.ObjectID{},
	}

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	result, InsErr := repo.client.Database("main").Collection("users").InsertOne(ctx, user)
	if InsErr != nil {
		log.Fatalf("database insert failed: %v", InsErr)
	}
	user.ID = result.InsertedID.(primitive.ObjectID)
	return &user, nil

}

func (repo *AccountsDBRepository) Login(login string, password string) (*models.User, error) {

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	filter := bson.M{"login": login}
	var user models.User

	err := repo.client.Database("main").Collection("users").FindOne(ctx, filter).Decode(&user)
	if err != nil {
		return nil, nil
	}

	err = bcrypt.CompareHashAndPassword([]byte(user.PasswordHash), []byte(password+login))
	if err != nil {
		return nil, errors.New("invalid password found")
	}

	return &user, nil
}

func (repo AccountsDBRepository) Update(user *models.User) (models.User, error) {

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	result, err := repo.client.Database("main").Collection("users").UpdateByID(ctx, user.ID, user)
	if err != nil {
		log.Fatalf("Update user failed, %v", result)
	}
	return *user, nil
}

func (repo AccountsDBRepository) DeleteAccount(user *models.User) bool {

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()
	_, err := repo.client.Database("main").Collection("users").DeleteOne(ctx, (bson.M{"_id": user.ID}))

	if err != nil {
		log.Fatalf("Delete account error: %v", err)
		return false
	}
	return true
}

func (repo AccountsDBRepository) GetUserByID(id primitive.ObjectID) models.User {

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	var user models.User

	err := repo.client.Database("main").Collection("users").FindOne(ctx, bson.M{"_id": id}).Decode(&user)
	if err != nil {
		log.Fatalf("User getting by id error %v", err)
	}
	return user
}
