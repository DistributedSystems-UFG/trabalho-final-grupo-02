import os
import asyncpg

pool = None

async def get_pool():
    global pool
    if pool is None:
        postgres_url = os.getenv("POSTGRES_URL", "postgresql://postgres:postgres@localhost:5432/inventory")
        pool = await asyncpg.create_pool(dsn=postgres_url)
    return pool

async def close_pool():
    global pool
    if pool is not None:
        await pool.close()
        pool = None
