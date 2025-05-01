import requests
import time

# Mila-ex API config
API_KEY = "your_api_key_here"
BASE_URL = "https://api.milaex.com/api/v1/exchange"

# Exchanges that support FetchTicker and BTC trading (GuaranteedPair used)
EXCHANGES = [
    {"key": "binance", "base": "BTC", "quote": "USDT"},
    {"key": "bitfinex", "base": "BTC", "quote": "USD"},
    {"key": "valr", "base": "BTC", "quote": "USDT"},
    {"key": "bitstamp", "base": "BTC", "quote": "USDT"},
    {"key": "coinbase", "base": "BTC", "quote": "USDT"},
    {"key": "coinex", "base": "BTC", "quote": "USDT"},
    {"key": "cryptocom", "base": "BTC", "quote": "USDT"},
    {"key": "gateio", "base": "BTC", "quote": "USDT"},
    {"key": "luno", "base": "BTC", "quote": "USDC"},
    {"key": "poloniex", "base": "BTC", "quote": "USDT"},
]

ARBITRAGE_THRESHOLD = 0.5  # Minimum profit % to flag an opportunity


# Fetch ticker price from a specific exchange
def get_price(exchange_key, base, quote):
    try:
        url = f"{BASE_URL}/ticker"
        headers = {"x-api-key": API_KEY}
        params = {
            "exchange": exchange_key,
            "base_name": base,
            "quote_name": quote
        }

        response = requests.get(url, headers=headers, params=params)
        response.raise_for_status()
        data = response.json()

        return float(data["Data"]["lastPrice"])
    except Exception as e:
        print(f"[ERROR] {exchange_key}: {e}")
        return None


# Compare prices and detect arbitrage opportunities
def check_arbitrage(prices):
    opportunities = []
    for buy in prices:
        for sell in prices:
            if buy["key"] == sell["key"] or buy["price"] is None or sell["price"] is None:
                continue

            diff = ((sell["price"] - buy["price"]) / buy["price"]) * 100
            if diff >= ARBITRAGE_THRESHOLD:
                opportunities.append({
                    "buy_from": buy["key"],
                    "sell_to": sell["key"],
                    "buy_price": buy["price"],
                    "sell_price": sell["price"],
                    "profit_percent": round(diff, 2)
                })

    return opportunities


# Main bot loop
def run_bot():
    while True:
        print("🔍 Checking BTC prices...")
        prices = []

        for ex in EXCHANGES:
            price = get_price(ex["key"], ex["base"], ex["quote"])
            prices.append({"key": ex["key"], "price": price})
            print(f"{ex['key']}: {price} {ex['quote']}")

        print("\n💡 Arbitrage Opportunities:")
        opps = check_arbitrage(prices)

        if opps:
            for opp in opps:
                print(f"💰 Buy from {opp['buy_from']} at {opp['buy_price']}, "
                      f"sell to {opp['sell_to']} at {opp['sell_price']} → "
                      f"Profit: {opp['profit_percent']}%")
        else:
            print("No arbitrage opportunities found.")

        print("\n⏳ Waiting 15 seconds...\n")
        time.sleep(15)


if __name__ == "__main__":
    run_bot()
