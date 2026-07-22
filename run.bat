@echo off
setlocal

if not exist ".venv" (
    echo Creating virtual environment...
    python -m venv .venv
)

call .venv\Scripts\activate.bat
pip install -q -r requirements.txt

if not exist ".env" (
    echo No .env found - copying .env.example. Edit it and add your FAL_KEY before generating images.
    copy .env.example .env
)

echo Starting Flux Generation Console at http://127.0.0.1:8000
start "" http://127.0.0.1:8000
python -m uvicorn backend.main:app --host 127.0.0.1 --port 8000
