from flask import Flask, request, jsonify
from flask_cors import CORS

app = Flask(__name__)

# Разрешаем ВСЕ запросы
CORS(app, resources={r"/*": {"origins": "*"}})

cart = []

@app.route('/cart', methods=['GET', 'OPTIONS'])
def get_cart():
    return jsonify(cart)

@app.route('/cart/add', methods=['POST', 'OPTIONS'])
def add_to_cart():
    if request.method == 'OPTIONS':
        return '', 200
    data = request.json
    cart.append(data)
    return jsonify(cart)

@app.route('/cart/clear', methods=['POST', 'OPTIONS'])
def clear_cart():
    if request.method == 'OPTIONS':
        return '', 200
    cart.clear()
    return jsonify({"message": "Корзина очищена"})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5002, debug=True)