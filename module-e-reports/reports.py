from flask import Flask, request, jsonify
from flask_cors import CORS

app = Flask(__name__)
CORS(app)

reports = []
report_counter = 1

@app.route('/reports', methods=['GET'])
def get_reports():
    return jsonify(reports)

@app.route('/reports/sales', methods=['POST'])
def create_sales_report():
    global report_counter
    data = request.json
    
    report = {
        "id": report_counter,
        "period": data.get('period', 'daily'),
        "total_sales": data.get('total', 0),
        "deals_count": data.get('deals_count', 0),
        "generated_at": str(__import__('datetime').datetime.now())
    }
    
    reports.append(report)
    report_counter += 1
    
    return jsonify(report), 201

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

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=3001, debug=True)