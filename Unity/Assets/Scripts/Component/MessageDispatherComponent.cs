﻿using System;
using System.Collections.Generic;

namespace Model
{
	[ObjectSystem]
	public class MessageDispatherComponentSystem : ObjectSystem<MessageDispatherComponent>, IAwake, ILoad
	{
		public void Awake()
		{
			this.Get().Awake();
		}

		public void Load()
		{
			this.Get().Load();
		}
	}

	/// <summary>
	/// 消息分发组件
	/// </summary>
	public class MessageDispatherComponent : Component
	{
		private readonly Dictionary<ushort, List<IMHandler>> handlers = new Dictionary<ushort, List<IMHandler>>();


		public void Awake()
		{
			this.Load();
		}

		public void Load()
		{
			this.handlers.Clear();

			Type[] types = DllHelper.GetMonoTypes();

			foreach (Type type in types)
			{
				object[] attrs = type.GetCustomAttributes(typeof(MessageHandlerAttribute), false);
				if (attrs.Length == 0)
				{
					continue;
				}

				IMHandler iMHandler = Activator.CreateInstance(type) as IMHandler;
				if (iMHandler == null)
				{
					Log.Error($"message handle {type.Name} 需要继承 IMHandler");
					continue;
				}

				Type messageType = iMHandler.GetMessageType();
				ushort opcode = this.Entity.GetComponent<OpcodeTypeComponent>().GetOpcode(messageType);
				if (opcode == 0)
				{
					Log.Error($"消息opcode为0: {messageType.Name}");
					continue;
				}
				this.RegisterHandler(opcode, iMHandler);
			}
		}

		public void RegisterHandler(ushort opcode, IMHandler handler)
		{
			Log.Debug($"11111111111111111111111 {opcode}");
			if (!this.handlers.ContainsKey(opcode))
			{
				this.handlers.Add(opcode, new List<IMHandler>());
			}
			this.handlers[opcode].Add(handler);
		}

		public void Handle(Session session, MessageInfo messageInfo)
		{
			List<IMHandler> actions;
			if (!this.handlers.TryGetValue(messageInfo.Opcode, out actions))
			{
				Log.Error($"消息 {messageInfo.Opcode} 没有处理");
				return;
			}
			
			foreach (IMHandler ev in actions)
			{
				try
				{
					ev.Handle(session, messageInfo.Message);
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());
				}
			}
		}

		public override void Dispose()
		{
			if (Id == 0)
			{
				return;
			}

			base.Dispose();
		}
	}
}