\# Форматы сообщений (JSON Schema)



\## 1. Запрос на создание лида (Модуль А)



```json

{

&#x20; "$schema": "http://json-schema.org/draft-07/schema#",

&#x20; "type": "object",

&#x20; "required": \["name", "phone"],

&#x20; "properties": {

&#x20;   "name": {

&#x20;     "type": "string",

&#x20;     "description": "Имя клиента",

&#x20;     "maxLength": 200,

&#x20;     "example": "Иван Петров"

&#x20;   },

&#x20;   "phone": {

&#x20;     "type": "string",

&#x20;     "description": "Номер телефона",

&#x20;     "pattern": "^\\\\+?\[0-9]{10,15}$",

&#x20;     "example": "+79991234567"

&#x20;   },

&#x20;   "email": {

&#x20;     "type": "string",

&#x20;     "format": "email",

&#x20;     "example": "ivan@example.com"

&#x20;   },

&#x20;   "source": {

&#x20;     "type": "string",

&#x20;     "enum": \["website", "call", "email", "partner"],

&#x20;     "default": "website"

&#x20;   }

&#x20; }

}

