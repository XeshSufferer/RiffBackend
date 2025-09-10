package models

type AcceptChatCreatingDTO struct {
	RequesterId   string `json:"requester"`
	RequestedId   string `json:"requested"`
	ChatId        string `json:"chat_id"`
	CorrelationId string `json:"correlation_id"`
}
