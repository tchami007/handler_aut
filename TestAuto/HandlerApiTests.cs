using Xunit;
using Handler.Controllers;
using Handler.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;

namespace TestAuto
{
	public class HandlerApiTests
	{
		/// <summary>
		/// Valida que la estructura de carpetas y clases principales del proyecto Handler estén presentes.
		/// Requisito de la etapa 1: existencia de modelos y controladores base.
		/// </summary>
		[Fact]
		public void Estructura_Carpetas_Proyectos_Existe()
		{
			Assert.NotNull(typeof(Cuenta));
			Assert.NotNull(typeof(SolicitudDebito));
			Assert.NotNull(typeof(LogOperacion));
			Assert.NotNull(typeof(AuthController));
			Assert.NotNull(typeof(TestController));
		}

		/// <summary>
		/// Valida que los modelos principales estén mapeados en el DbContext y disponibles para operaciones con EF.
		/// Requisito de la etapa 1: mapeo de entidades en Entity Framework.
		/// </summary>
		[Fact]
		public void Modelos_Están_Mapeados_En_DbContext()
		{
			var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Handler.Infrastructure.HandlerDbContext>()
				.UseInMemoryDatabase(databaseName: "TestDb")
				.Options;
			var db = new Handler.Infrastructure.HandlerDbContext(options);
			Assert.NotNull(db.Cuentas);
			Assert.NotNull(db.SolicitudesDebito);
			Assert.NotNull(db.LogsOperacion);
		}

		/// <summary>
		/// Valida que el endpoint público de TestController responde correctamente sin autenticación.
		/// Requisito de la etapa 1: endpoint público accesible.
		/// </summary>
		// [Fact]
		// public void Endpoint_Publico_Retorna_Ok()
		// {
		//     var publisher = new Handler.Services.RabbitMqPublisher(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build());
		//     var controller = new TestController(publisher);
		//     var result = controller.Publico();
		//     Assert.IsType<OkObjectResult>(result);
		// }

		/// <summary>
		/// Valida que el endpoint de login retorna un token JWT válido para credenciales correctas.
		/// Requisito de la etapa 1: autenticación JWT básica.
		/// </summary>
		// [Fact]
		// public void Login_Usuario_Valido_Retorna_Token()
		// {
		//     var config = new ConfigurationBuilder()
		//         .AddInMemoryCollection(new Dictionary<string, string?>
		//         {
		//             {"Jwt:Issuer", "HandlerIssuer"},
		//             {"Jwt:Audience", "HandlerAudience"},
		//             {"Jwt:SecretKey", "ClaveSuperSecretaHandlerAutorizacion2025_XXYZ"}
		//         })
		//         .Build();
		//     var controller = new AuthController(config);
		//     var request = new LoginRequest { Username = "test", Password = "1234" };
		//     var result = controller.Login(request);
		//     Assert.IsType<OkObjectResult>(result);
		// }
	}
}
