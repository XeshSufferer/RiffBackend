package models

type UserLoginData struct {
	CorellationId string `json:"correlation_id"`
	Login         string `json:"login"`
	Password      string `json:"password"`
}
