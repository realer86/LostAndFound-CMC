"""Issue #2 basic browser tests for the standalone UI mockup.

Requirements: pip install playwright; playwright install chromium
Run from repository root: python tests/issue02_ui_smoke.py
Tests are independent from Docker, API and PostgreSQL.
"""
from pathlib import Path
import os
from playwright.sync_api import sync_playwright

PROJECT_ROOT = Path(__file__).resolve().parents[1]
SITE_DIR = PROJECT_ROOT / 'frontend/public/ui-homepage'



def main():
    # Inline assets, so this test does not require a network listener or Docker.
    html = (SITE_DIR / 'index.html').read_text(encoding='utf-8')
    html = html.replace('<link rel="stylesheet" href="./styles.css">', '')
    html = html.replace('<script src="./app.js" defer></script>', '')
    css = (SITE_DIR / 'styles.css').read_text(encoding='utf-8')
    js = (SITE_DIR / 'app.js').read_text(encoding='utf-8')
    with sync_playwright() as playwright:
        options = {'headless': True}
        if os.environ.get('CHROMIUM_PATH'):
            options['executable_path'] = os.environ['CHROMIUM_PATH']
        browser = playwright.chromium.launch(**options)
        for width, height in [(320, 700), (375, 812), (390, 844), (768, 1024), (1024, 768), (1440, 900)]:
            page = browser.new_page(viewport={'width': width, 'height': height})
            errors = []
            page.on('pageerror', lambda error: errors.append(str(error)))
            page.set_content(html, wait_until='load')
            page.add_style_tag(content=css)
            page.add_script_tag(content=js)
            assert page.locator('#item-grid article').count() == 6, (width, 'expected 6 demo cards')
            assert not page.evaluate('document.documentElement.scrollWidth > window.innerWidth'), (width, 'horizontal overflow on homepage')
            page.get_by_role('button', name='Đồ nhặt được', exact=True).click()
            assert page.locator('#item-grid article').count() == 3, (width, 'filter not applied')
            page.get_by_role('button', name='Tất cả', exact=True).click()
            page.locator('#search-input').fill('tai nghe')
            assert page.locator('#item-grid article').count() == 1, (width, 'search mismatch')
            page.locator('#search-input').fill('')
            # Navigate through a direct anchor; it remains reachable on mobile.
            page.locator('.hero-actions [data-type="LOST"]').click()
            assert page.locator('#post-view').is_visible(), (width, 'post view hidden')
            assert not page.evaluate('document.documentElement.scrollWidth > window.innerWidth'), (width, 'horizontal overflow on form')
            page.locator('#item-name').fill('Balo đen')
            page.locator('#category').select_option('Ví & túi xách')
            page.locator('#location').select_option('Thư viện')
            page.locator('#event-time').fill('2026-10-18T10:30')
            page.locator('#description').fill('Màu đen, có logo tròn phía trước.')
            page.get_by_role('button', name='Xem trước tin đăng').click()
            assert page.locator('#preview-modal').is_visible(), (width, 'preview did not open')
            assert 'Balo đen' in page.locator('#preview-content').inner_text()
            page.get_by_role('button', name='Quay lại chỉnh sửa').click()
            assert not errors, (width, errors)
            page.close()
            print(f'PASS {width}x{height}: responsive, filter, search, form preview, no JS errors')
        browser.close()


if __name__ == '__main__':
    main()
