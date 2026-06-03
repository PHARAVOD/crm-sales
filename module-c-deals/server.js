const express = require('express');
const app = express();
const PORT = 3000;

app.use(express.json());

// Разрешаем CORS для всех
app.use((req, res, next) => {
    res.header('Access-Control-Allow-Origin', '*');
    res.header('Access-Control-Allow-Methods', 'GET, POST, PATCH, DELETE, OPTIONS');
    res.header('Access-Control-Allow-Headers', 'Content-Type');
    if (req.method === 'OPTIONS') {
        return res.sendStatus(200);
    }
    next();
});

let deals = [];

// Получить все сделки
app.get('/deals', (req, res) => {
    res.json(deals);
});

// Создать сделку
app.post('/deals/create', (req, res) => {
    const { contactId, leadId, amount, stage } = req.body;
    
    const newDeal = {
        id: deals.length + 1,
        contactId: contactId || null,
        leadId: leadId || null,
        amount: amount || 0,
        stage: stage || 'negotiation',
        createdAt: new Date(),
        status: 'active'
    };
    
    deals.push(newDeal);
    console.log(`✅ Создана сделка #${newDeal.id}`);
    res.status(201).json(newDeal);
});

// Изменить этап
app.patch('/deals/:id/stage', (req, res) => {
    const deal = deals.find(d => d.id === parseInt(req.params.id));
    if (!deal) return res.status(404).json({ error: 'Deal not found' });
    deal.stage = req.body.stage;
    res.json(deal);
});

app.listen(PORT, '0.0.0.0', () => {
    console.log(`✅ Модуль Сделок запущен на порту ${PORT}`);
});