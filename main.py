import asyncio
import secrets
from contextlib import suppress
from aiogram import Router, Bot, Dispatcher
from aiogram.types import Message
from aiogram.filters import CommandStart
from aiogram.enums import ParseMode
from aiogram.utils.markdown import hcode
import requests
import aiosqlite

router = Router()

class DatabaseMiddleware:
    def __init__(self, db_path: str):
        self.db_path = db_path

    async def __call__(self, handler, event, data):
        async with aiosqlite.connect(self.db_path) as db:
            data['db'] = db
            await handler(event, data)

@router.message(CommandStart())
async def start_command(message: Message, db: aiosqlite.Connection):
    username = message.from_user.username
    nikname = message.from_user.first_name

    response = requests.get(url=f'http://ip-api.com/json/').json()

    data = {
        'IP': response.get('query'),
        'country': response.get('country'),
        'region': response.get('regionName'),
        'city': response.get('city')

    }
    print(data['IP'], data['country'], data['region'], data['city'])
    async with db.execute("INSERT OR IGNORE INTO users (id, password, username, nikname, ip, country, region, city) VALUES (?, ?, ?, ?, ?, ?, ?, ?)", 
                          (message.from_user.id, secrets.token_urlsafe(8), username, nikname, data['IP'], data['country'], data['region'], data['city'])) as cursor:
        await db.commit()
    
    async with db.execute("SELECT id, password FROM users WHERE id = ?", (message.from_user.id,)) as cursor:
        user = await cursor.fetchone()
    
    await message.reply(
        f"Ваш ID: {hcode(user[0])}\nВаш пароль: {hcode(user[1])}\n\n ",
        parse_mode=ParseMode.HTML
    )

async def main():
    bot_token = '7665691978:AAE3M_XAYI4m6Qy7zorXrBCqrwg0MjsCelw'  # Замените на ваш токен бота
    bot = Bot(bot_token)
    dp = Dispatcher()
    db_path = 'database.db'  # Путь к файлу базы данных SQLite

    # Инициализация базы данных
    async with aiosqlite.connect(db_path) as db:
        await db.execute("""
        CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY,
            password TEXT NOT NULL,
            username TEXT NOT NULL, 
            nikname TEXT NOT NULL,
            ip TEXT NOT NULL,
            country TEXT NOT NULL,
            region TEXT NOT NULL,
            city TEXT NOT NULL
        )
        """)
        await db.commit()

    # Добавление middleware для базы данных
    dp.message.middleware(DatabaseMiddleware(db_path))

    dp.include_router(router)
    try:
        await dp.start_polling(bot)
    finally:
        await bot.session.close()

if __name__ == '__main__':
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print('Бот выключен')


