package models

type UserRegisterData struct {
	CorrelationID string `json:"correlation_id"`
	Nickname      string `json:"nickname"`
	Password      string `json:"password"`
	Login         string `json:"login"`
}
