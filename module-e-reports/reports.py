version: '3.8'

services:
  # Модуль А: Лиды и Товары (Python/Flask)
  module-a-leads:
    build: ./module-a-leads
    container_name: crm-leads
    ports:
      - "5001:5001"
    environment:
      - FLASK_ENV=production
    depends_on:
      - postgres
      - redis
    networks:
      - crm-network

  # Модуль Б: Корзина (Python/Flask)
  module-b-cart:
    build: ./module-b-cart
    container_name: crm-cart
    ports:
      - "5002:5002"
    environment:
      - FLASK_ENV=production
    depends_on:
      - redis
    networks:
      - crm-network

  # Модуль В: Сделки (Node.js/Express)
  module-c-deals:
    build: ./module-c-deals
    container_name: crm-deals
    ports:
      - "3000:3000"
    depends_on:
      - postgres
    networks:
      - crm-network

  # Модуль D: Задачи (Python/FastAPI)
  module-d-tasks:
    build: ./module-d-tasks
    container_name: crm-tasks
    ports:
      - "5003:5003"
    depends_on:
      - postgres
    networks:
      - crm-network

  # Модуль E: Отчеты (Python/Flask)
  module-e-reports:
    build: ./module-e-reports
    container_name: crm-reports
    ports:
      - "3001:3001"
    depends_on:
      - postgres
    networks:
      - crm-network

  # PostgreSQL база данных
  postgres:
    image: postgres:15-alpine
    container_name: crm-postgres
    environment:
      POSTGRES_USER: crm_user
      POSTGRES_PASSWORD: crm_password
      POSTGRES_DB: crm_database
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - crm-network

  # Redis для кэширования и сессий
  redis:
    image: redis:7-alpine
    container_name: crm-redis
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    networks:
      - crm-network

  # Nginx как reverse proxy
  nginx:
    image: nginx:alpine
    container_name: crm-nginx
    ports:
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./index.html:/usr/share/nginx/html/index.html
    depends_on:
      - module-a-leads
      - module-b-cart
      - module-c-deals
      - module-d-tasks
      - module-e-reports
    networks:
      - crm-network

volumes:
  postgres_data:
  redis_data:

networks:
  crm-network:
    driver: bridge