from flask import Flask, request, jsonify
from flask_cors import CORS

app = Flask(__name__)
CORS(app)

cart = []


@app.route('/cart', methods=['GET'])
def get_cart():
    """Получить содержимое корзины"""
    return jsonify(cart)


@app.route('/cart/add', methods=['POST'])
def add_to_cart():
    """Добавить товар в корзину"""
    data = request.json
    cart.append(data)
    return jsonify(cart)


@app.route('/cart/clear', methods=['POST'])
def clear_cart():
    """Очистить корзину"""
    cart.clear()
    return jsonify({"message": "Корзина очищена"})


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5002, debug=True)