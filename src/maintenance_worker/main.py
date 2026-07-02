from dotenv import load_dotenv
load_dotenv()

import asyncio
from consumer import start_consumer
from db import close_pool

async def main():
    print("Maintenance Worker is starting...")
    try:
        await start_consumer()
    finally:
        await close_pool()

if __name__ == "__main__":
    asyncio.run(main())
