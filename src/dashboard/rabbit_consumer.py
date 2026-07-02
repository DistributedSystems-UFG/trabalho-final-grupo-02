import aio_pika
import json
import os
from websocket_manager import manager

async def start_consumer():
    rabbitmq_url = os.getenv("RABBITMQ_URL", "amqp://admin:admin@localhost/")
    
    import asyncio
    while True:
        try:
            connection = await aio_pika.connect_robust(rabbitmq_url)
            break
        except Exception as e:
            print(f"Waiting for RabbitMQ to start... ({e})")
            await asyncio.sleep(2)
    channel = await connection.channel()
    
    # Declare the exchange (should match .NET side)
    exchange = await channel.declare_exchange("inventory", aio_pika.ExchangeType.TOPIC, durable=True)
    
    # Declare and bind the queue
    queue = await channel.declare_queue("alerts.dashboard", durable=True)
    await queue.bind(exchange, routing_key="inventory.#")
    
    async with queue.iterator() as queue_iter:
        async for message in queue_iter:
            async with message.process():
                payload = json.loads(message.body.decode())
                # Forward to all connected WebSocket clients
                await manager.broadcast(json.dumps(payload))
                print(f" [x] Forwarded: {payload}")
