export interface AuthUser {
  userId: number
  username: string
  displayName: string
  role: 'Admin' | 'Maker' | 'Checker'
  token: string
}
