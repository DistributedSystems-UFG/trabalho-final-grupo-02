from fastapi import FastAPI, WebSocket, WebSocketDisconnect, Request
from fastapi.templating import Jinja2Templates
from contextlib import asynccontextmanager
import asyncio
from websocket_manager import manager
from rabbit_consumer import start_consumer
from db import get_pool, close_pool
from dotenv import load_dotenv

load_dotenv()

@asynccontextmanager
async def lifespan(app: FastAPI):
    # Start RabbitMQ consumer in the background
    consumer_task = asyncio.create_task(start_consumer())
    yield
    # Cleanup on shutdown
    consumer_task.cancel()
    await close_pool()

app = FastAPI(lifespan=lifespan)
templates = Jinja2Templates(directory="templates")

@app.get("/")
async def root(request: Request):
    return templates.TemplateResponse(request=request, name="index.html")

@app.get("/api/products")
async def get_products():
    pool = await get_pool()
    async with pool.acquire() as conn:
        rows = await conn.fetch("SELECT id, name, category, quantity, price FROM products ORDER BY id")
        return [dict(row) for row in rows]

@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    await manager.connect(websocket)
    try:
        while True:
            await websocket.receive_text() # Keep connection alive
    except WebSocketDisconnect:
        manager.disconnect(websocket)
