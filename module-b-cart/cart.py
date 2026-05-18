from flask import Flask, request, jsonify
from flask_cors import CORS

app = Flask(__name__)
CORS(app)  # РАЗРЕШАЕМ ВСЕ ЗАПРОСЫ

cart_items = []

@app.route('/cart', methods=['GET'])
def show_cart():
    return jsonify(cart_items)

@app.route('/cart/add', methods=['POST'])
def add_to_cart():
    item = request.get_json()
    cart_items.append(item)
    return jsonify(cart_items)

@app.route('/cart/clear', methods=['POST'])
def clear_cart():
    cart_items.clear()
    return jsonify({"message": "Очищено"})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5002, debug=True)