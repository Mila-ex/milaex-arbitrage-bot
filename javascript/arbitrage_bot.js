const axios = require("axios");

const API_KEY = "your_api_key_here";
const BASE_URL = "https://api.milaex.com/api/v1/exchange";

// Exchanges with supported guaranteed BTC quote pairs
const EXCHANGES = [
  { key: "binance", base: "BTC", quote: "USDT" },
  { key: "bitfinex", base: "BTC", quote: "USD" },
  { key: "valr", base: "BTC", quote: "USDT" },
  { key: "bitstamp", base: "BTC", quote: "USDT" },
  { key: "coinbase", base: "BTC", quote: "USDT" },
  { key: "coinex", base: "BTC", quote: "USDT" },
  { key: "cryptocom", base: "BTC", quote: "USDT" },
  { key: "gateio", base: "BTC", quote: "USDT" },
  { key: "luno", base: "BTC", quote: "USDC" },
  { key: "poloniex", base: "BTC", quote: "USDT" }
];

const ARBITRAGE_THRESHOLD = 0.5; // percent

async function getPrice(exchangeKey, base, quote) {
  try {
    const response = await axios.get(`${BASE_URL}/ticker`, {
      params: {
        exchange: exchangeKey,
        base_name: base,
        quote_name: quote
      },
      headers: {
        "x-api-key": API_KEY
      }
    });

    return parseFloat(response.data.Data.lastPrice);
  } catch (error) {
    console.error(`Error from ${exchangeKey}:`, error.response?.data?.Message || error.message);
    return null;
  }
}

function checkArbitrage(prices) {
  const opportunities = [];
  for (const buy of prices) {
    for (const sell of prices) {
      if (buy.key === sell.key || !buy.price || !sell.price) continue;

      const diff = ((sell.price - buy.price) / buy.price) * 100;
      if (diff >= ARBITRAGE_THRESHOLD) {
        opportunities.push({
          buyFrom: buy.key,
          sellTo: sell.key,
          buyPrice: buy.price,
          sellPrice: sell.price,
          profitPercent: diff.toFixed(2)
        });
      }
    }
  }
  return opportunities;
}

async function runBot() {
  while (true) {
    console.log("🔍 Checking BTC prices...");
    const prices = [];

    for (const ex of EXCHANGES) {
      const price = await getPrice(ex.key, ex.base, ex.quote);
      prices.push({ key: ex.key, price });
      console.log(`${ex.key}: ${price} ${ex.quote}`);
    }

    console.log("\n💡 Arbitrage Opportunities:");
    const opps = checkArbitrage(prices);
    if (opps.length) {
      opps.forEach(opp => {
        console.log(`💰 Buy from ${opp.buyFrom} at ${opp.buyPrice}, sell to ${opp.sellTo} at ${opp.sellPrice} → Profit: ${opp.profitPercent}%`);
      });
    } else {
      console.log("No arbitrage opportunities found.");
    }

    console.log("\n⏳ Waiting 15 seconds...\n");
    await new Promise(resolve => setTimeout(resolve, 15000));
  }
}

runBot();
