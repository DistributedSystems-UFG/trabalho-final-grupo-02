import asyncio
import aio_pika
import json
import os

from reconciler import reconcile

MAX_CONCURRENT_TASKS = 5
semaphore = asyncio.Semaphore(MAX_CONCURRENT_TASKS)

async def process_message(message: aio_pika.IncomingMessage):
    async with message.process():
        async with semaphore:
            payload = json.loads(message.body.decode())
            print(f" [x] Processing maintenance task: {payload}")
            await reconcile(payload)

async def start_consumer():
    rabbitmq_url = os.getenv("RABBITMQ_URL", "amqp://admin:admin@localhost/")
    
    while True:
        try:
            connection = await aio_pika.connect_robust(rabbitmq_url)
            break
        except Exception as e:
            print(f"Waiting for RabbitMQ to start... ({e})")
            await asyncio.sleep(2)
    channel = await connection.channel()
    
    exchange = await channel.declare_exchange("inventory", aio_pika.ExchangeType.TOPIC, durable=True)
    
    queue = await channel.declare_queue("maintenance.tasks", durable=True)
    await queue.bind(exchange, routing_key="inventory.stock.low")
    
    print(" [*] Waiting for maintenance tasks. To exit press CTRL+C")
    await queue.consume(process_message)
    
    # Keep the consumer running
    try:
        await asyncio.Future()
    finally:
        await connection.close()
