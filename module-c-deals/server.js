const express = require('express');
const app = express();
const PORT = 3000;

app.use(express.json());

let deals = [];
let productsCatalog = []; // Справочник товаров из модуля А

// ========== СУЩЕСТВУЮЩИЕ ЭНДПОИНТЫ ==========
app.get('/deals', (req, res) => {
    res.json(deals);
});

app.post('/deals/create', (req, res) => {
    const { contactId, leadId, amount, stage } = req.body;
    
    const newDeal = {
        id: deals.length + 1,
        contactId,
        leadId,
        amount,
        stage: stage || 'negotiation',
        createdAt: new Date(),
        status: 'active'
    };
    
    deals.push(newDeal);
    res.status(201).json(newDeal);
});

app.patch('/deals/:id/stage', (req, res) => {
    const deal = deals.find(d => d.id === parseInt(req.params.id));
    if (!deal) return res.status(404).json({ error: 'Deal not found' });
    
    deal.stage = req.body.stage;
    res.json(deal);
});

// ========== НОВЫЙ: ВЕБХУК ДЛЯ ПРИЕМА ТОВАРОВ ==========
app.post('/api/webhooks/product-created', (req, res) => {
    const { event, timestamp, data } = req.body;
    
    console.log(`📦 Получен вебхук: ${event} в ${timestamp}`);
    console.log(`📦 Товар: ${data.product_name} (${data.price} руб)`);
    
    // Сохраняем товар в локальный справочник
    const product = {
        id: data.product_id,
        name: data.product_name,
        price: data.price,
        category: data.category,
        syncedAt: new Date()
    };
    
    // Проверяем, есть ли уже такой товар
    const existingIndex = productsCatalog.findIndex(p => p.id === product.id);
    if (existingIndex !== -1) {
        productsCatalog[existingIndex] = product;
        console.log(`🔄 Товар обновлен в справочнике`);
    } else {
        productsCatalog.push(product);
        console.log(`✅ Товар добавлен в справочник`);
    }
    
    res.json({ 
        received: true, 
        product: product,
        catalogSize: productsCatalog.length 
    });
});

// ========== НОВЫЙ: ПОЛУЧИТЬ СПРАВОЧНИК ТОВАРОВ ==========
app.get('/api/products-catalog', (req, res) => {
    res.json({
        products: productsCatalog,
        count: productsCatalog.length,
        lastSync: new Date()
    });
});

app.listen(PORT, () => {
    console.log(`✅ Модуль Сделок запущен на порту ${PORT}`);
    console.log(`📡 Вебхук эндпоинт: http://localhost:${PORT}/api/webhooks/product-created`);
});