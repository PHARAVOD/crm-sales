from fastapi import FastAPI, HTTPException, Request
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List, Optional
from datetime import datetime
import uvicorn

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

tasks_db = []
products_catalog = []  # Справочник товаров из модуля А
task_counter = 1

class TaskCreate(BaseModel):
    title: str
    description: Optional[str] = None
    deal_id: int
    assigned_to: str
    due_date: str

class Task(TaskCreate):
    id: int
    status: str = "pending"

class WebhookProduct(BaseModel):
    event: str
    timestamp: str
    data: dict

# ========== СУЩЕСТВУЮЩИЕ ЭНДПОИНТЫ ==========
@app.get("/tasks", response_model=List[Task])
def get_tasks():
    return tasks_db

@app.post("/tasks/create", response_model=Task)
def create_task(task: TaskCreate):
    global task_counter
    new_task = Task(
        id=task_counter,
        **task.dict(),
        status="pending"
    )
    tasks_db.append(new_task)
    task_counter += 1
    return new_task

@app.patch("/tasks/{task_id}/complete")
def complete_task(task_id: int):
    for task in tasks_db:
        if task.id == task_id:
            task.status = "completed"
            return {"message": "Task completed", "task": task}
    raise HTTPException(404, "Task not