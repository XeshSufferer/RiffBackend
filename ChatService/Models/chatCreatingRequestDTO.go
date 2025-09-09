package models

type ChatCreatingRequestDTO struct {
	RequesterId       string `json:"requester_id"`
	RequestedUsername string `json:"requested_username"`
	CorrelationId     string `json:"correlation_id"`
}
