package models

type LocalChatCreatingRequest struct {
	RequesterId   string `json:"requester_id"`
	RequestedId   string `json:"requested_id"`
	RequestedUser User   `json:"requested_user"`
	RequesterUser User   `json:"requester_user"`
	CorrelationId string `json:"correlation_id"`
}
