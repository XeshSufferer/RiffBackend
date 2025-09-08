package models

type AcceptChatCreatingDTO struct {
	Requester     User   `json:"requester"`
	Requested     User   `json:"requested"`
	ChatId        string `json:"chat_id"`
	CorrelationId string `json:"correlation_id"`
}
