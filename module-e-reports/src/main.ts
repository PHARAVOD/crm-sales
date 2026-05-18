import { NestFactory } from '@nestjs/core';
import { Controller, Get, Post, Body, Module } from '@nestjs/common';

// Контроллер для отчетов
@Controller('reports')
class ReportsController {
  private reports = [];

  @Get()
  getReports() {
    return this.reports;
  }

  @Post('sales')
  createSalesReport(@Body() body: any) {
    const report = {
      id: this.reports.length + 1,
      period: body.period || 'daily',
      total_sales: body.total || 0,
      deals_count: body.deals_count || 0,
      generated_at: new Date()
    };
    this.reports.push(report);
    return report;
  }
}

// Модуль приложения
@Module({
  controllers: [ReportsController],
})
class AppModule {}

// Запуск
async function bootstrap() {
  const app = await NestFactory.create(AppModule);
  
  // Включаем CORS
  app.enableCors({
    origin: '*',
    methods: 'GET,POST,PUT,DELETE',
    allowedHeaders: '*',
  });
  
  await app.listen(3001);
  console.log('✅ Модуль Отчетов запущен на порту 3001');
}

bootstrap();