from flask import Flask, jsonify
from flask_cors import CORS

app = Flask(__name__)
CORS(app)

# Товары
products = [
    {"id": 1, "name": "Ноутбук", "price": 50000},
    {"id": 2, "name": "Мышь", "price": 1000},
    {"id": 3, "name": "Клавиатура", "price": 3000},
    {"id": 4, "name": "Монитор", "price": 25000}
]

# Лиды
leads = [
    {"id": 1, "name": "Иван Петров", "phone": "+79991234567", "status": "new"},
    {"id": 2, "name": "Мария Сидорова", "phone": "+79997654321", "status": "contacted"}
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
    from flask import request
    import uuid
    
    data = request.json
    new_lead = {
        "id": len(leads) + 1,
        "name": data.get('name'),
        "phone": data.get('phone'),
        "email": data.get('email'),
        "status": "new"
    }
    leads.append(new_lead)
    return jsonify(new_lead), 201
# Контакты (временно, для теста)
contacts = [
    {"id": 1, "name": "Иван Петров", "phone": "+79991234567", "email": "ivan@example.com"},
    {"id": 2, "name": "Мария Сидорова", "phone": "+79997654321", "email": "maria@example.com"}
]

@app.route('/contacts', methods=['GET'])
def get_contacts():
    return jsonify(contacts)

@app.route('/contacts', methods=['POST'])
def create_contact():
    data = request.json
    new_contact = {
        "id": len(contacts) + 1,
        "name": data.get('name'),
        "phone": data.get('phone'),
        "email": data.get('email')
    }
    contacts.append(new_contact)
    return jsonify(new_contact), 201

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001, debug=True)