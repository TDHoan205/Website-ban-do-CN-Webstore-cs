from ddgs import DDGS

try:
    with DDGS() as ddgs:
        results = [r for r in ddgs.images("iPhone 15 Pro Max", max_results=2)]
        print(results)
except Exception as e:
    print(f"Error: {e}")
