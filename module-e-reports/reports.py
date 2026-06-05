from flask import Flask, request, jsonify
from flask_cors import CORS
from datetime import datetime

app = Flask(__name__)
CORS(app)

reports = []
report_counter = 1


@app.route('/reports', methods=['GET'])
def get_reports():
    """Получить список отчетов"""
    return jsonify(reports)


@app.route('/reports/sales', methods=['POST'])
def create_sales_report():
    """Создать отчет о продажах"""
    global report_counter
    data = request.json

    report = {
        "id": report_counter,
        "period": data.get('period', 'daily'),
        "total_sales": data.get('total', 0),
        "deals_count": data.get('deals_count', 0),
        "generated_at": datetime.now().isoformat()
    }

    reports.append(report)
    report_counter += 1
    return jsonify(report), 201


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=3001, debug=True)