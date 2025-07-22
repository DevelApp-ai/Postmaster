import { HubConnectionBuilder, LogLevel, HubConnection } from '@microsoft/signalr';
import { createMachine, interpret } from 'xstate';
import { PostmasterConfig } from './types';

export class PostmasterService {
  private connection: HubConnection | null = null;
  private service: any = null;
  private config: PostmasterConfig;

  constructor(config: PostmasterConfig) {
    this.config = config;
  }

  /**
   * Initializes and starts the Postmaster service
   */
  public async start(): Promise<void> {
    try {
      const token = await this.config.tokenProvider();
      
      // Build the connection
      this.connection = new HubConnectionBuilder()
        .withUrl(this.config.hubUrl, { accessTokenFactory: () => token })
        .configureLogging(LogLevel.Information)
        .withAutomaticReconnect()
        .build();

      // Set up message handlers
      this.connection.on('ReceiveMessage', (message) => {
        if (this.config.onMessageReceived) {
          this.config.onMessageReceived(message);
        }
      });

      // Start the connection
      await this.connection.start();
      
      if (this.config.onConnected) {
        this.config.onConnected();
      }
    } catch (error) {
      if (this.config.onError) {
        this.config.onError(error as Error);
      }
      throw error;
    }
  }

  /**
   * Stops the Postmaster service
   */
  public async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      
      if (this.config.onDisconnected) {
        this.config.onDisconnected();
      }
    }
  }

  /**
   * Sends a message to a specific user
   */
  public async sendToUser(recipientId: string, content: string): Promise<void> {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    
    await this.connection.invoke('SendToUser', recipientId, content);
  }

  /**
   * Sends a message to a service
   */
  public async sendToService(serviceName: string, content: string): Promise<any> {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    
    return await this.connection.invoke('SendToService', serviceName, content);
  }

  /**
   * Sends a message to a group
   */
  public async sendToGroup(groupName: string, content: string): Promise<void> {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    
    await this.connection.invoke('SendToGroup', groupName, content);
  }

  /**
   * Marks a message as read
   */
  public async markAsRead(messageId: string): Promise<void> {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    
    await this.connection.invoke('MarkAsRead', messageId);
  }

  /**
   * Gets unread messages for the current user
   */
  public async getUnreadMessages(): Promise<any[]> {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    
    return await this.connection.invoke('GetUnreadMessages');
  }

  /**
   * Gets messages for the current user
   */
  public async getUserMessages(isInbound: boolean, fromDate?: Date): Promise<any[]> {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    
    return await this.connection.invoke('GetUserMessages', isInbound, fromDate);
  }
}
