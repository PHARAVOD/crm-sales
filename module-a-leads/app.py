from flask import Flask, jsonify
from flask_cors import CORS

app = Flask(__name__)
CORS(app)  # РАЗРЕШАЕМ ВСЕ ЗАПРОСЫ

products = [
    {"id": 1, "name": "Ноутбук", "price": 50000},
    {"id": 2, "name": "Мышь", "price": 1000},
    {"id": 3, "name": "Клавиатура", "price": 3000}
]

@app.route('/products', methods=['GET'])
def get_products():
    return jsonify(products)

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001, debug=True)