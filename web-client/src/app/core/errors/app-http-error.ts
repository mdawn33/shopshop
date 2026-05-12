
export class AppHttpError {
    
    constructor(readonly status: number, readonly message: string, readonly body: unknown) {
    }

}