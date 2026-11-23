from html.parser import HTMLParser
from pathlib import Path
from django.conf import settings
from django.contrib.auth.models import User
from django.test import TestCase


class ResourceParser(HTMLParser):
    def __init__(self):
        super().__init__()
        self.resources = []

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag in ('img', 'link'):
            url = attrs.get('src') or attrs.get('href', '')
            if url.startswith('/static/game/'):
                self.resources.append(url.removeprefix('/static/'))


class GamePortalTests(TestCase):
    def test_public_progress_and_local_resources(self):
        response = self.client.get('/')
        self.assertContains(response, 'Valle del Eco')
        self.assertContains(response, 'Templo de NÉMESIS')
        self.assertContains(response, 'name="usuario"')
        self.assertContains(response, 'name="password1"')
        self.check_resources(response)

    def check_resources(self, response):
        parser = ResourceParser()
        parser.feed(response.content.decode())
        for resource in parser.resources:
            self.assertTrue((Path(settings.BASE_DIR) / 'game/static' / resource).is_file(), resource)

    def test_private_pages_and_existing_login_flow(self):
        paths = ['/inicio2/', '/personajes/', '/como-jugar/', '/novedades/', '/contacto/']
        for path in paths:
            self.assertRedirects(self.client.get(path), '/')
        User.objects.create_user(username='explorador', password='test-echo-password')
        self.assertRedirects(self.client.post('/login/', {
            'usuario': 'explorador', 'password': 'test-echo-password'
        }), '/inicio2/')
        for path in paths:
            response = self.client.get(path)
            self.assertEqual(response.status_code, 200)
            self.check_resources(response)
        self.assertContains(self.client.get('/como-jugar/'), 'Ctrl + dirección')
        self.assertNotContains(self.client.get('/novedades/'), 'MK-II')
        self.assertRedirects(self.client.get('/logout/'), '/')
        self.assertRedirects(self.client.get('/inicio2/'), '/')

    def test_offline_and_worker_still_available(self):
        self.assertEqual(self.client.get('/offline/').status_code, 200)
        worker = self.client.get('/service-worker.js')
        self.assertEqual(worker['Content-Type'], 'application/javascript')
        self.assertEqual(worker['Service-Worker-Allowed'], '/')
        self.assertContains(worker, 'primal-echo-v4')
