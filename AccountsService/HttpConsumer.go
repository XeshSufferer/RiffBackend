package main

import (
	models "AccountsService/Models"

	"github.com/gin-gonic/gin"
)

func InitializeHttpConsume(db *AccountsDBRepository) {
	router := gin.Default()

	_db := db

	router.GET("/userExistByName/:name", func(c *gin.Context) {
		name := c.Param("name")

		if name == "" {
			c.JSON(400, models.BooleanResponse{Success: false})
			return
		}

		user := _db.GetUserByName(name)
		if user.ID.IsZero() {
			c.JSON(404, models.BooleanResponse{Success: false})
			return
		}

		c.JSON(200, models.BooleanResponse{Success: true})
	})

	router.GET("/userExistById/:id", func(ctx *gin.Context) {
		_id := ctx.Param("id")

		user := _db.GetUserByHexID(_id)

		if user.ID.IsZero() {
			ctx.JSON(404, models.BooleanResponse{Success: false})
			return
		}

		ctx.JSON(200, models.BooleanResponse{Success: true})
	})

	router.POST("/user/:id/addToChat/:chatId", func(c *gin.Context) {
		userID := c.Param("id")
		chatID := c.Param("chatId")

		if userID == "" || chatID == "" {
			c.JSON(400, models.BooleanResponse{Success: false})
			return
		}

		user := _db.GetUserByHexID(userID)
		if user.ID.IsZero() {
			c.JSON(400, models.BooleanResponse{Success: false})
			return
		}

		_db.AddUserToChat(userID, chatID)

		c.JSON(200, models.BooleanResponse{Success: true})
	})

	router.GET("/getUserIdByName/:name", func(ctx *gin.Context) {

		_name := ctx.Param("name")

		if _name == "" {
			ctx.JSON(400, models.StringResponse{Result: ""})
			return
		}

		user := _db.GetUserByName(_name)

		if user.ID.IsZero() {
			ctx.JSON(404, models.StringResponse{Result: ""})
			return
		}

		ctx.JSON(200, models.StringResponse{Result: user.ID.Hex()})
	})

	go func() {
		router.Run(":8081")
	}()
}
