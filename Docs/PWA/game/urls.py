from django.urls import path
from . import views

urlpatterns = [

    path('', views.inicio, name='inicio'),

    path(
        'login/',
        views.login_view,
        name='login'
    ),

    path(
        'registro/',
         views.registro,
         name='registro'
         ),

    path(
        'novedades/',
        views.novedades,
        name='novedades'
    ),

    path(
        'personajes/',
        views.personajes,
        name='personajes'
    ),

    path(
        'como-jugar/',
        views.como_jugar,
        name='como_jugar'
    ),

    path(
        'contacto/',
        views.contacto,
        name='contacto'
    ),
    path(
        'inicio2/',
        views.inicio2,
        name='inicio2'
    ),

    path(
        'logout/',
        views.logout_view,
        name='logout'
    ),

    path(
        'offline/',
        views.offline,
        name='offline'
        ),

    path('manifest.webmanifest', views.web_manifest, name='web_manifest'),
    path(
        'service-worker.js',
        views.service_worker,
        name='service_worker'
        ),

]
