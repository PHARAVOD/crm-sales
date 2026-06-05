from flask import Flask, jsonify, request
from flask_cors import CORS

app = Flask(__name__)
CORS(app)


products = [
    {"id": 1, "name": "Ноутбук", "price": 50000},
    {"id": 2, "name": "Мышь", "price": 1000},
    {"id": 3, "name": "Клавиатура", "price": 3000},
    {"id": 4, "name": "Монитор", "price": 25000}
]

leads = [
    {"id": 1, "name": "Иван Петров", "phone": "+79991234567", "status": "new"},
    {"id": 2, "name": "Мария Сидорова", "phone": "+79997654321", "status": "contacted"}
]

contacts = [
    {"id": 1, "name": "Иван Петров", "phone": "+79991234567", "email": "ivan@example.com"},
    {"id": 2, "name": "Мария Сидорова", "phone": "+79997654321", "email": "maria@example.com"}
]


@app.route('/products', methods=['GET'])
def get_products():
    """Получить список товаров"""
    return jsonify(products)


@app.route('/leads', methods=['GET'])
def get_leads():
    """Получить список лидов"""
    return jsonify(leads)


@app.route('/leads', methods=['POST'])
def create_lead():
    """Создать нового лида"""
    data = request.json
    new_lead = {
        "id": len(leads) + 1,
        "name": data.get('name'),
        "phone": data.get('phone'),
        "status": "new"
    }
    leads.append(new_lead)
    return jsonify(new_lead), 201


@app.route('/contacts', methods=['GET'])
def get_contacts():
    """Получить список контактов"""
    return jsonify(contacts)


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001, debug=True)