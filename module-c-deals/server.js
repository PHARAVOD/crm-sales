const express = require('express');
const app = express();
const PORT = 3000;

app.use(express.json());

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

app.get('/deals', (req, res) => {
    res.json(deals);
});

app.post('/deals/create', (req, res) => {
    const { contactId, leadId, amount, stage } = req.body;

    const newDeal = {
        id: deals.length + 1,
        contactId: contactId || null,
        leadId: leadId || null,
        amount: amount || 0,
        stage: stage || 'new',
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

app.listen(PORT, '0.0.0.0', () => {
    console.log(`✅ Module Deals running on port ${PORT}`);
});