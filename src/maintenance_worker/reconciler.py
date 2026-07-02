import socket
from db import get_pool

worker_id = socket.gethostname()

async def reconcile(payload: dict):
    product_id = payload.get("productId")
    current_stock = payload.get("currentStock", 0)
    
    if product_id is None:
        return

    # Restock logic: order 50 new units
    new_qty = current_stock + 50
    reason = f"Stock dropped below threshold to {current_stock}. Auto-reconciliation triggered restock."

    pool = await get_pool()
    async with pool.acquire() as conn:
        async with conn.transaction():
            # Update product quantity
            await conn.execute(
                "UPDATE products SET quantity = quantity + $1, updated_at = NOW() WHERE id = $2",
                50, product_id
            )
            
            # Insert into reconciliation_log
            await conn.execute(
                """
                INSERT INTO reconciliation_log (product_id, old_qty, new_qty, reason, worker_id, created_at)
                VALUES ($1, $2, $3, $4, $5, NOW())
                """,
                product_id, current_stock, new_qty, reason, worker_id
            )
