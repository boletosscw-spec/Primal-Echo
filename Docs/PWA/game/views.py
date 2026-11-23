from django.shortcuts import render, redirect
from django.contrib.auth.models import User
from django.contrib.auth import login, authenticate
from django.contrib import messages
from django.template.loader import render_to_string
from django.http import HttpResponse


def login_view(request):

    if request.method == "POST":

        usuario = request.POST.get("usuario", "").strip()
        password = request.POST.get("password", "")

        # Buscar y verificar al usuario en la base de datos
        user = authenticate(
            request,
            username=usuario,
            password=password
        )

        if user is not None:

            # Iniciar sesión
            login(request, user)

            # Guardar datos de sesión
            request.session["logueado"] = True
            request.session["usuario"] = user.username

            return redirect("/inicio2/")

        else:
            messages.error(request, "Usuario o contraseña incorrectos.")
            return redirect("/")

    return redirect("/")


def inicio(request):
    return render(
        request,
        'game/inicio.html'
    )


def inicio2(request):

    if not request.session.get("logueado"):
        return redirect("/")

    return render(
        request,
        'game/inicio2.html',
        {
            'usuario': request.session.get('usuario')
        }
    )


def novedades(request):

    if not request.session.get("logueado"):
        return redirect("/")

    return render(
        request,
        'game/novedades.html',
        {
            'usuario': request.session.get('usuario')
        }
    )


def personajes(request):

    if not request.session.get("logueado"):
        return redirect("/")

    return render(
        request,
        'game/personajes.html',
        {
            'usuario': request.session.get('usuario')
        }
    )


def como_jugar(request):

    if not request.session.get("logueado"):
        return redirect("/")

    return render(
        request,
        'game/como_jugar.html',
        {
            'usuario': request.session.get('usuario')
        }
    )


def contacto(request):

    if not request.session.get("logueado"):
        return redirect("/")

    return render(
        request,
        'game/contacto.html',
        {
            'usuario': request.session.get('usuario')
        }
    )


def logout_view(request):

    request.session.flush()

    return redirect("/")



def registro(request):

    if request.method == 'POST':

        username = request.POST.get('username')
        email = request.POST.get('email')
        password1 = request.POST.get('password1')
        password2 = request.POST.get('password2')

        if password1 != password2:

            messages.error(request, 'Las contraseñas no coinciden.')

            return redirect('/')

        if User.objects.filter(username=username).exists():

            messages.error(request, 'El usuario ya existe.')

            return redirect('/')

        if User.objects.filter(email__iexact=email).exists():

            messages.error(request, 'El correo ya está registrado.')

            return redirect('/login/')

        usuario = User.objects.create_user(
            username=username,
            email=email,
            password=password1
        )

        login(request, usuario)

        return redirect('/')

    return redirect('/')

# PÁGINA SIN CONEXIÓN
def offline(request):
    return render(request, 'game/offline.html')


# SERVICE WORKER
def service_worker(request):

    contenido = render_to_string('game/service-worker.js')

    response = HttpResponse(
        contenido,
        content_type='application/javascript'
    )

    response['Service-Worker-Allowed'] = '/'

    return response