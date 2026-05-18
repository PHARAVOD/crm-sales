import requests
import json
import threading
from datetime import datetime

class WebhookSender:
    """Отправляет вебхуки при создании/изменении товаров"""
    
    def __init__(self):
        # Адреса для отправки вебхуков
        self.webhook_urls = [
            "http://localhost:3000/api/webhooks/product-created",  # Модуль В (Сделки)
            "http://localhost:5003/webhooks/product-created",     # Модуль D (Задачи)
            "http://localhost:3001/webhooks/product-created",     # Модуль E (Отчеты)
        ]
    
    def send_product_created(self, product_data):
        """Отправляет вебхук о создании товара"""
        
        webhook_data = {
            "event": "ProductCreated",
            "timestamp": datetime.utcnow().isoformat(),
            "data": {
                "product_id": product_data.get('id'),
                "product_name": product_data.get('name'),
                "price": product_data.get('price'),
                "description": product_data.get('description'),
                "category": product_data.get('category')
            }
        }
        
        # Отправляем в отдельном потоке, чтобы не блокировать основной код
        for url in self.webhook_urls:
            thread = threading.Thread(
                target=self._send_webhook,
                args=(url, webhook_data)
            )
            thread.start()
    
    def send_product_updated(self, product_data):
        """Отправляет вебхук об обновлении товара"""
        
        webhook_data = {
            "event": "ProductUpdated",
            "timestamp": datetime.utcnow().isoformat(),
            "data": {
                "product_id": product_data.get('id'),
                "product_name": product_data.get('name'),
                "price": product_data.get('price'),
                "changes": product_data.get('changes', {})
            }
        }
        
        for url in self.webhook_urls:
            thread = threading.Thread(
                target=self._send_webhook,
                args=(url, webhook_data)
            )
            thread.start()
    
    def _send_webhook(self, url, data):
        """Отправляет один вебхук"""
        try:
            response = requests.post(
                url,
                json=data,
                headers={"Content-Type": "application/json"},
                timeout=5
            )
            if response.status_code == 200:
                print(f"✅ Webhook отправлен на {url}")
            else:
                print(f"⚠️ Webhook на {url} вернул {response.status_code}")
        except Exception as e:
            print(f"❌ Ошибка отправки webhook на {url}: {e}")