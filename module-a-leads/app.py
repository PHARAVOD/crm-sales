from flask import Flask, request, jsonify
from flask_cors import CORS
from webhooks.webhook_sender import WebhookSender

app = Flask(__name__)
CORS(app)

# Создаем отправитель вебхуков
webhook_sender = WebhookSender()

# База данных товаров (в памяти)
products = [
    {"id": 1, "name": "Ноутбук", "price": 50000, "category": "Электроника"},
    {"id": 2, "name": "Мышь", "price": 1000, "category": "Электроника"},
    {"id": 3, "name": "Клавиатура", "price": 3000, "category": "Электроника"}
]

@app.route('/products', methods=['GET'])
def get_products():
    return jsonify(products)

@app.route('/products', methods=['POST'])
def create_product():
    """Создание товара с отправкой вебхука"""
    data = request.json
    
    new_product = {
        "id": len(products) + 1,
        "name": data.get('name'),
        "price": data.get('price'),
        "category": data.get('category')
    }
    
    products.append(new_product)
    
    # ОТПРАВЛЯЕМ ВЕБХУК В ДРУГИЕ МОДУЛИ
    webhook_sender.send_product_created(new_product)
    
    return jsonify(new_product), 201

@app.route('/products/<int:product_id>', methods=['PUT'])
def update_product(product_id):
    """Обновление товара с отправкой вебхука"""
    data = request.json
    
    for product in products:
        if product['id'] == product_id:
            changes = {}
            if 'name' in data and product['name'] != data['name']:
                changes['name'] = {'old': product['name'], 'new': data['name']}
                product['name'] = data['name']
            
            if 'price' in data and product['price'] != data['price']:
                changes['price'] = {'old': product['price'], 'new': data['price']}
                product['price'] = data['price']
            
            # ОТПРАВЛЯЕМ ВЕБХУК ОБ ОБНОВЛЕНИИ
            webhook_sender.send_product_updated({
                'id': product_id,
                'name': product['name'],
                'price': product['price'],
                'changes': changes
            })
            
            return jsonify(product)
    
    return jsonify({"error": "Product not found"}), 404

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001, debug=True)