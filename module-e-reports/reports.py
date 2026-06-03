from flask import Flask, request, jsonify
from flask_cors import CORS
from datetime import datetime

app = Flask(__name__)
CORS(app)

# Хранилище отчетов
reports = []
report_counter = 1

# Получить все отчеты
@app.route('/reports', methods=['GET'])
def get_reports():
    return jsonify(reports)

# Создать отчет о продажах
@app.route('/reports/sales', methods=['POST'])
def create_sales_report():
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

# Получить сводку по отчетам
@app.route('/reports/summary', methods=['GET'])
def get_summary():
    total_sales = sum(r.get('total_sales', 0) for r in reports)
    total_deals = sum(r.get('deals_count', 0) for r in reports)
    
    return jsonify({
        "total_reports": len(reports),
        "total_sales": total_sales,
        "total_deals": total_deals,
        "average_sale": total_sales / total_deals if total_deals > 0 else 0
    })

# Получить отчет по ID
@app.route('/reports/<int:report_id>', methods=['GET'])
def get_report(report_id):
    for report in reports:
        if report['id'] == report_id:
            return jsonify(report)
    return jsonify({"error": "Report not found"}), 404

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=3001, debug=True)