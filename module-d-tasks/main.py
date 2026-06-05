from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List, Optional
import uvicorn

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

tasks_db = []
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


@app.get("/tasks", response_model=List[Task])
def get_tasks():
    """Получить список задач"""
    return tasks_db


@app.post("/tasks/create", response_model=Task)
def create_task(task: TaskCreate):
    """Создать новую задачу"""
    global task_counter
    new_task = Task(
        id=task_counter,
        title=task.title,
        description=task.description,
        deal_id=task.deal_id,
        assigned_to=task.assigned_to,
        due_date=task.due_date,
        status="pending"
    )
    tasks_db.append(new_task)
    task_counter += 1
    return new_task


@app.patch("/tasks/{task_id}/complete")
def complete_task(task_id: int):
    """Отметить задачу выполненной"""
    for task in tasks_db:
        if task.id == task_id:
            task.status = "completed"
            return {"message": "Task completed", "task": task.dict()}
    raise HTTPException(status_code=404, detail="Task not found")


if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=5003)